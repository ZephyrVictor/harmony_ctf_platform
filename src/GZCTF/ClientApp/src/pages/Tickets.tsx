import {
  Badge,
  Center,
  Group,
  Pagination,
  ScrollArea,
  Stack,
  Table,
  Text,
  Title,
} from '@mantine/core'
import dayjs from 'dayjs'
import { FC, useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { showErrorMsg } from '@Utils/Shared'
import { getMyTickets, TicketDto } from '@Utils/ExtApi'
import { Role } from '@Api'

const Tickets: FC = () => {
  const { t } = useTranslation()
  const [tickets, setTickets] = useState<TicketDto[]>([])
  const [loading, setLoading] = useState(true)
  const [page, setPage] = useState(1)
  const perPage = 20

  const load = useCallback(async () => {
    try {
      const data = await getMyTickets(200, 0)
      setTickets(data)
    } catch (err: any) {
      showErrorMsg(err, t)
    } finally {
      setLoading(false)
    }
  }, [t])

  useEffect(() => {
    load()
  }, [load])

  const pagedTickets = tickets.slice((page - 1) * perPage, page * perPage)

  return (
    <WithNavBar width="90%">
      <WithRole requiredRole={Role.User}>
        <Stack gap="md">
          <Title order={4}>{t('game.content.ticket.my_tickets')}</Title>
          {tickets.length === 0 && !loading ? (
            <Center h="10rem">
              <Text c="dimmed">{t('admin.ticket.empty')}</Text>
            </Center>
          ) : (
            <>
              <ScrollArea type="never" h="calc(100vh - 20rem)">
                <Table>
                  <Table.Thead>
                    <Table.Tr>
                      <Table.Th>{t('admin.ticket.col.challenge')}</Table.Th>
                      <Table.Th>{t('admin.ticket.col.description')}</Table.Th>
                      <Table.Th>{t('admin.ticket.col.status')}</Table.Th>
                      <Table.Th>{t('admin.ticket.col.time')}</Table.Th>
                      <Table.Th>{t('game.content.ticket.refund_bonus')}</Table.Th>
                    </Table.Tr>
                  </Table.Thead>
                  <Table.Tbody>
                    {pagedTickets.map((ticket) => (
                      <Table.Tr key={ticket.id}>
                        <Table.Td>
                          <Text size="sm">{ticket.challengeName ?? '—'}</Text>
                        </Table.Td>
                        <Table.Td>
                          <Text size="sm" lineClamp={2}>{ticket.description}</Text>
                        </Table.Td>
                        <Table.Td>
                          <Badge
                            color={
                              ticket.status === 'Approved' ? 'green'
                              : ticket.status === 'Rejected' ? 'red'
                              : 'yellow'
                            }
                            variant="light"
                          >
                            {ticket.status}
                          </Badge>
                        </Table.Td>
                        <Table.Td>
                          <Text size="sm">{dayjs(ticket.createdAt).format('L LT')}</Text>
                        </Table.Td>
                        <Table.Td>
                          {ticket.status === 'Approved' ? (
                            <Text size="sm" c="green">
                              +{ticket.refundScore + ticket.bonusScore}
                            </Text>
                          ) : ticket.status === 'Rejected' ? (
                            <Text size="sm" c="dimmed">{ticket.reviewComment ?? '—'}</Text>
                          ) : (
                            <Text size="sm" c="dimmed">—</Text>
                          )}
                        </Table.Td>
                      </Table.Tr>
                    ))}
                  </Table.Tbody>
                </Table>
              </ScrollArea>
              <Pagination
                value={page}
                onChange={setPage}
                total={Math.ceil(tickets.length / perPage)}
              />
            </>
          )}
        </Stack>
      </WithRole>
    </WithNavBar>
  )
}

export default Tickets
